# Estratégia de Margem Fixa de Dinheiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o exemplo "Money Fixed Margin" do MetaTrader usando a API de alto nível do StockSharp. Ela demonstra como dimensionar posições arriscando uma porcentagem fixa do portfólio enquanto converte a distância do stop-loss expressa em pips para um deslocamento de preço absoluto. A estratégia opera apenas posições compradas e foca em demonstrar a lógica de gestão do dinheiro em vez de um sinal de entrada preditivo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: executa uma compra a mercado após cada contagem de velas concluída especificada por `Check Interval` (padrão a cada 980ª barra). A ordem usa o preço de fechamento da vela desencadeante como referência para cálculos de risco.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O stop-loss protetor é anexado automaticamente via `StartProtection` a uma distância derivada do parâmetro `Stop Loss (pips)`.
  - Nenhum alvo de lucro é usado; as posições fecham apenas pelo stop-loss ou intervenção manual.
- **Stops**: Apenas Stop Loss.
- **Valores padrão**:
  - `Stop Loss (pips)` = 25
  - `Risk Percent` = 10
  - `Check Interval` = 980
  - `Candle Type` = período de 1 minuto
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Sim (stop-loss)
  - Complexidade: Básico
  - Período: Intradiário (configurável através de `Candle Type`)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio (escala com `Risk Percent`)

## Lógica de Dimensionamento de Posição

1. A estratégia lê `Security.PriceStep` e `Security.Decimals` para inferir o tamanho do pip. Pares com 3 ou 5 casas decimais usam um multiplicador décuplo para corresponder à definição de pip do MetaTrader.
2. `Stop Loss (pips)` é multiplicado pelo tamanho do pip para obter uma distância de preço absoluta (`ExtStopLoss`) idêntica ao código MQL5.
3. O valor atual do portfólio (preferindo `Portfolio.CurrentValue` e depois `Portfolio.BeginValue`) é multiplicado por `Risk Percent / 100` para determinar o capital exposto por trade.
4. O risco por lote único é calculado através do produto da distância do stop-loss, o número de passos de preço dentro dessa distância e `Security.StepPrice` quando disponível. Se `StepPrice` for desconhecido, a própria distância de preço é usada como fallback.
5. A divisão do valor de risco pelo risco por lote produz o volume desejado. O resultado é normalizado para o `VolumeStep` do instrumento, limitado aos limites mínimo e máximo de volume, e registrado para transparência. Um valor de comparação com distância de stop-loss zero também é registrado para ilustrar por que o gestor de dinheiro recusa trades sem um stop protetor.

## Fluxo de Trabalho

1. Ao iniciar, a estratégia assina a série de velas configurada, calcula o tamanho do pip e habilita `StartProtection` com a distância de stop-loss absoluta calculada.
2. Cada vela concluída incrementa um contador interno. Quando o contador atinge o `Check Interval` escolhido, a estratégia avalia o tamanho da posição, imprime informações de diagnóstico e redefine o contador.
3. Se o volume calculado for positivo, uma ordem de compra a mercado é colocada. A proteção integrada anexa o stop-loss em `Close - ExtStopLoss`. Quaisquer erros (por exemplo, devido a dados insuficientes ou instrumentos com preço zero) impedem o envio da ordem.
4. Nenhum trade adicional é realizado até que o contador complete outro intervalo, mantendo o foco na gestão do dinheiro em vez da frequência de sinais.

## Notas de Uso

- Defina `Risk Percent` para um valor conservador ao conectar a uma conta ao vivo; o risco padrão de 10% espelha o exemplo MQL, mas é agressivo para trading real.
- Certifique-se de que o instrumento forneça metadados significativos de `PriceStep` e `StepPrice`. Quando indisponíveis, a estratégia ainda opera mas interpreta o risco em unidades de preço bruto.
- A estratégia evita intencionalmente trades vendidos para manter fidelidade à demonstração original. Adapte as chamadas `BuyMarket`/`SellMarket` se o trading bidirecional for desejado.
- Combine este módulo de gestão do dinheiro com outros geradores de sinais reutilizando o helper `CalculateFixedMarginVolume` do código da estratégia.

# Estratégia MACross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o comportamento do consultor especialista `MQL/34176/MACross.mq4` original usando o StockSharp API de alto nível. Ele negocia um único instrumento em um cruzamento de média móvel e mantém todos os controles de risco expressos em pips e patrimônio líquido.

## Lógica de negociação

1. Duas médias móveis simples (SMA) são construídas com base no tipo de vela configurado:
   - `FastPeriod` reage rapidamente às mudanças de preço.
   - `SlowPeriod` suaviza a tendência de longo prazo.
2. No fechamento de cada vela finalizada, as médias rápida e lenta são comparadas:
   - Um cruzamento de alta (cruzamento rápido acima do lento) abre uma posição longa. Qualquer venda ativa é achatada primeiro.
   - Um cruzamento de baixa (cruzamento rápido abaixo do lento) abre uma posição curta após fechar uma posição longa existente.
3. Cada entrada utiliza um volume de mercado fixo derivado de `LotSize` e alinhado com os limites do instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`).
4. Depois que a posição é aberta, a estratégia rastreia duas metas de risco medidas em pips. O tamanho do pip é inferido automaticamente de `Security.Decimals` (ou `PriceStep` como substituto):
   - `TakeProfitPips` define a distância até a meta de lucro. Acertá-lo provoca uma saída do mercado na direção atual.
   - `StopLossPips` define a distância de parada protetora. Quebrá-lo fecha a posição imediatamente.
5. A negociação pode ser pausada pelo guarda `MinEquity`. Quando o valor atual da carteira está abaixo do limite a estratégia continua gerenciando a posição ativa mas não permite novas entradas.

Todos os cálculos funcionam apenas em velas finalizadas, correspondendo totalmente ao consultor especialista original que esperou por uma nova barra antes de avaliar as médias móveis.

## Visualização

Quando um painel de gráfico está disponível, a estratégia traça:

- Insira velas da série assinada.
- Os SMAs rápidos e lentos.
- Negociações próprias para destacar entradas e saídas acionadas pelas regras de cruzamento.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `8` | Comprimento do SMA rápido que gera sinais de cruzamento. |
| `SlowPeriod` | `int` | `20` | Comprimento da lenta SMA usada como linha de tendência de referência. |
| `TakeProfitPips` | `decimal` | `20` | Distância alvo de lucro expressa em pips. O tamanho do pip é inferido a partir dos decimais do instrumento. |
| `StopLossPips` | `decimal` | `20` | Distância de parada protetora em pips. Usa o mesmo cálculo do tamanho do pip que a meta de lucro. |
| `LotSize` | `decimal` | `1` | Volume básico do pedido. A estratégia arredonda para o tamanho permitido mais próximo antes de enviar ordens de mercado. |
| `MinEquity` | `decimal` | `100` | Patrimônio mínimo da conta. Novas negociações são bloqueadas enquanto o valor da carteira estiver abaixo deste nível. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Série de velas usada para cálculos SMA e avaliação de sinal. |

## Diferenças vs. versão MQL

- O especialista original MQL passou os preços de stop loss e take-profit para `OrderSend` como zero. A porta StockSharp emula o mesmo comportamento com saídas manuais que monitoram o preço de fechamento de cada vela finalizada.
- A validação de patrimônio (`cekMinEquity`) agora lê `Portfolio.CurrentValue` e `Portfolio.BeginValue` em vez de `AccountEquity()`, mas preserva a lógica de limite.
- A detecção do tamanho do pip espelha o auxiliar `GetPipPoint`: cotações de 2 ou 3 dígitos usam 0,01, cotações de 4 ou 5 dígitos usam 0,0001, caso contrário, `PriceStep` é usado.

A estratégia resultante pode ser otimizada por meio de todos os parâmetros expostos e combina perfeitamente com StockSharp gráficos e infraestrutura de gerenciamento de risco.

# Estratégia Exp DEMA Canal de Faixa Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Exp DEMA Range Channel Tm Plus porta o consultor especialista MetaTrader original para a API de alto nível do StockSharp. Ela constrói um canal de média móvel exponencial dupla (DEMA) ao redor dos extremos de preço e interpreta as cores dos candles produzidas pelo canal para decidir quando operar. A implementação mantém a lógica de gerenciamento de dinheiro simples, confiando na propriedade `Volume` da plataforma e ordens de proteção opcionais enquanto reproduz as regras de rompimento e tempo de espera do código fonte.

## Lógica principal

- **Construção do canal**
  - Dois indicadores DEMA com o mesmo período são calculados: um nas máximas dos candles e outro nas mínimas.
  - Suas saídas são deslocadas para frente por um número configurável de barras (`Shift`) para corresponder a como o indicador personalizado original desenha o canal.
  - Um deslocamento de preço em pontos (`PriceShiftPoints`) pode ser adicionado para alargar ou estreitar o canal.
- **Cores de sinal**
  - Um candle que fecha acima da banda superior deslocada é considerado altista.
  - Um candle que fecha abaixo da banda inferior deslocada é considerado baixista.
  - A direção do corpo do candle (fechamento ≥ abertura ou fechamento ≤ abertura) é preservada para imitar as quatro cores possíveis (0–3) do indicador MQL.
- **Condições de entrada**
  - A estratégia olha para trás `SignalBar` barras para avaliar a última cor de rompimento e confirma que a barra anterior não já mostrou o mesmo sinal. Isso captura o momento em que um novo rompimento aparece.
  - Entradas compradas só são permitidas quando `EnableBuyEntry` é verdadeiro e a cor detectada corresponde a um rompimento para cima.
  - Entradas vendidas requerem `EnableSellEntry` e um rompimento para baixo.
- **Condições de saída**
  - Posições compradas podem ser fechadas em qualquer rompimento para baixo se `EnableBuyExit` estiver habilitado.
  - Posições vendidas podem ser fechadas em rompimentos para cima se `EnableSellExit` estiver habilitado.
  - Posições também podem ser fechadas após um tempo de manutenção configurável (`HoldingMinutes`) se `UseHoldingLimit` for verdadeiro, refletindo o filtro de tempo do consultor especialista.
- **Controle de risco**
  - Distâncias opcionais de take-profit e stop-loss (em pontos de preço) ativam `StartProtection`, que emite ordens de proteção usando execução de mercado quando os limites são atingidos.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `MaPeriod` | Período DEMA usado para as linhas do canal superior e inferior. |
| `Shift` | Número de barras que as linhas DEMA são deslocadas para frente antes das comparações. |
| `PriceShiftPoints` | Distância adicional, medida em pontos de preço (múltiplos de `PriceStep`), adicionada à linha superior e subtraída da linha inferior. |
| `SignalBar` | Número de barras para trás usado para avaliar a cor de rompimento. `0` significa barra atual, `1` a última barra fechada, etc. |
| `EnableBuyEntry` / `EnableSellEntry` | Alternador para entradas de rompimento compradas e vendidas. |
| `EnableBuyExit` / `EnableSellExit` | Alternador para sair de posições compradas ou vendidas em sinais opostos. |
| `UseHoldingLimit` | Habilita o fechamento de posições após `HoldingMinutes` minutos no mercado. |
| `HoldingMinutes` | Tempo máximo de manutenção antes de um fechamento forçado; definir como `0` para desabilitar enquanto mantém o flag verdadeiro. |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias de proteção em pontos de preço. Quando maiores que zero são convertidas em deslocamentos absolutos de preço e passadas para `StartProtection`. |
| `CandleType` | Tipo de candle e período usado para todos os cálculos (padrão candles de 8 horas como no script MQL). |

## Fluxo de trading

1. Assinar candles definidos por `CandleType` e iniciar os indicadores DEMA.
2. Armazenar os valores de canal mais recentes em filas para que o algoritmo possa referenciar o valor que existia `Shift` barras antes, reproduzindo o deslocamento do indicador original.
3. Quando um candle finaliza, calcular sua cor de rompimento e adicioná-la ao buffer deslizante. Usar o buffer para identificar novos rompimentos para cima ou para baixo de acordo com `SignalBar`.
4. Fechar posições existentes se o sinal oposto aparecer ou se o filtro de tempo expirar.
5. Entrar em novas operações enviando ordens de mercado com tamanho `Volume + |Position|` para girar do lado oposto quando necessário.
6. Atualizar o timestamp interno da posição ativa para manter o filtro de tempo de manutenção preciso.

## Notas

- A estratégia assume que os dados do gráfico são processados em ordem cronológica. Ao executar em backtests ou trading ao vivo, garantir que o fluxo de candles esteja ordenado para manter o comportamento de deslocamento correto.
- `Volume` deve ser definido na estratégia antes da inicialização (via UI ou código) para controlar o dimensionamento de posição. Os modos de gerenciamento de dinheiro do especialista MQL não são intencionalmente replicados.
- Como as ordens de proteção são opcionais, lembrar de configurar os valores de stop-loss e take-profit ao implantar em ambientes de produção.
- O auxiliar de gráfico desenha candles e operações executadas automaticamente, permitindo verificação visual de que os rompimentos do canal acionam as entradas e saídas esperadas.

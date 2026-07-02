# Estratégia Hpcs Inter7
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Hpcs Inter7 é um sistema de breakout de bandas Bollinger convertido do MetaTrader 4 consultor especialista `_HPCS_Inter7_MT4_EA_V01_We.mq4`. O algoritmo monitora bandas Bollinger padrão calculadas na série de velas selecionada. Quando o preço ultrapassa as bandas, ele interpreta isso como um rompimento de impulso e abre uma posição na direção do rompimento. Para cada nova entrada, a estratégia coloca imediatamente as metas de stop loss e takeprofit a uma distância fixa do preço de entrada para replicar o comportamento original do consultor especialista.

## Lógica de negociação
- **Entrada curta**: Quando a vela anterior fechou acima da banda inferior e a última vela fechada terminou abaixo da banda inferior, a estratégia abre uma venda no mercado. Isso recria a condição original `Close[0] < LowerBand[0] && Close[1] > LowerBand[1]`.
- **Entrada longa**: Quando a vela anterior fechou abaixo da banda superior e a última vela fechada terminou acima da banda superior, a estratégia abre uma compra no mercado. Isso replica `Close[0] > UpperBand[0] && Close[1] < UpperBand[1]` da implementação MQL.
- **Negociação única por vela**: O algoritmo lembra o horário de abertura da vela que gerou a última ordem. Um novo sinal na mesma vela é ignorado para evitar negociações duplicadas, espelhando a variável de guarda `gdt_Candle` de MQL4.
- **Ordens de proteção**: Imediatamente após a abertura de uma nova posição, a estratégia chama `SetStopLoss` e `SetTakeProfit` usando a distância configurada. Ambos são colocados simetricamente em torno do preço de entrada para que a posição tenha sempre metas de risco e recompensa predefinidas.

## Parâmetros
| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `BollingerLength` | Número de velas usadas para construir as bandas Bollinger. | 20 | Sim |
| `BollingerDeviation` | Multiplicador de desvio padrão para a largura das bandas Bollinger. | 2 | Sim |
| `CandleType` | Série de velas usada para cálculos (o padrão é o período de 1 minuto). | Velas de 1 minuto | Não |
| `ProtectionDistancePoints` | Distância Stop Loss e Take Profit expressa em etapas de preço. | 10 | Sim |

## Notas adicionais
- A estratégia usa o StockSharp API de alto nível (`SubscribeCandles().Bind(...)`) e não armazena matrizes de histórico personalizadas.
- `StartProtection()` é ativado no início para que a plataforma gerencie automaticamente as ordens de proteção feitas por `SetStopLoss` e `SetTakeProfit`.
- O tamanho da posição é controlado pela propriedade base `Strategy.Volume`, assim como o consultor especialista original que negociou um volume fixo de um lote.
- A estratégia foi projetada para instrumentos FX onde o EA original foi implantado, mas pode ser usada em qualquer segurança que forneça sinais de banda Bollinger significativos e um valor `PriceStep` válido.

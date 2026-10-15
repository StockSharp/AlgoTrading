# Estratégia XFatlXSatlCloud Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
XFatlXSatlCloud Duplex é uma estratégia bidirecional convertida do expert advisor original do MQL5. Ela opera cruzamentos do indicador XFatlXSatlCloud, que combina um filtro digital FATL rápido com um filtro SATL mais lento e, em seguida, suaviza ambos com médias móveis configuráveis. Configurações separadas podem ser aplicadas aos lados comprado e vendido, incluindo diferentes períodos, métodos de suavização e fontes de preço aplicadas.

## Lógica de trading
A estratégia avalia apenas velas terminadas. Duas assinaturas independentes são executadas em paralelo: uma impulsiona a lógica comprada e a outra a lógica vendida. Cada assinatura alimenta o indicador XFatlXSatlCloud implementado em C# e produz o seguinte comportamento:

- **Entrada comprada** – acionada quando a linha rápida cruza acima da linha lenta na barra definida por `LongSignalBar`. Se uma posição vendida estiver aberta, ela é fechada primeiro (somente se `ShortAllowClose` estiver habilitado). Uma ordem de compra a mercado com `LongVolume` contratos é então enviada e o preço de entrada é registrado para verificações de risco.
- **Saída comprada** – executada quando a linha rápida cai abaixo da linha lenta na barra deslocada. Verificações opcionais de stop-loss e take-profit baseadas em preço (`LongStopLoss`, `LongTakeProfit`) podem fechar a posição mais cedo se o range da vela violar os deslocamentos definidos.
- **Entrada vendida** – acionada quando a linha rápida cruza abaixo da linha lenta na barra definida por `ShortSignalBar`. A exposição comprada existente é nivelada primeiro se `LongAllowClose` estiver habilitado. Uma ordem de venda a mercado com `ShortVolume` contratos é enviada em seguida.
- **Saída vendida** – executada quando a linha rápida sobe acima da linha lenta na barra deslocada. Controles de risco opcionais (`ShortStopLoss`, `ShortTakeProfit`) monitoram extremos intrabarra.

Todos os valores de indicadores são calculados apenas em velas terminadas, garantindo que cada decisão se baseie em dados finais e espelhe o comportamento MQL original.

## Gestão de risco
A estratégia rastreia o último preço de entrada separadamente para posições compradas e vendidas. Se um offset de stop-loss ou take-profit for especificado e a vela atual violar o limiar correspondente, a posição é fechada imediatamente (sujeito à bandeira `AllowClose` relevante). Os offsets são medidos em unidades de preço absolutas do instrumento negociado.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Trading | `LongVolume` | Tamanho da ordem para entradas compradas (maior que zero). |
| Trading | `ShortVolume` | Tamanho da ordem para entradas vendidas (maior que zero). |
| Trading | `LongAllowOpen` | Habilitar ou desabilitar a abertura de novas posições compradas. |
| Trading | `LongAllowClose` | Habilitar ou desabilitar saídas compradas (necessário para stops e saídas cruzadas). |
| Trading | `ShortAllowOpen` | Habilitar ou desabilitar a abertura de novas posições vendidas. |
| Trading | `ShortAllowClose` | Habilitar ou desabilitar saídas vendidas. |
| Signals | `LongSignalBar` | Número de barras concluídas a olhar para trás ao verificar o cruzamento para comprados. |
| Signals | `ShortSignalBar` | Número de barras concluídas a olhar para trás ao verificar o cruzamento para vendidos. |
| Data | `LongCandleType` | Tipo de vela (período) usado para a assinatura do indicador comprado. |
| Data | `ShortCandleType` | Tipo de vela usado para a assinatura do indicador vendido. |
| Indicators | `LongMethod1` | Método de suavização aplicado à saída FATL no lado comprado. Valores suportados: SMA, EMA, SMMA, LWMA, Jurik, ZeroLag, Kaufman. |
| Indicators | `LongLength1` | Comprimento do suavizador rápido comprado. |
| Indicators | `LongPhase1` | Parâmetro de fase encaminhado ao suavizador rápido (mantido por compatibilidade, apenas Jurik o usa conceitualmente). |
| Indicators | `LongMethod2` | Método de suavização aplicado à saída SATL no lado comprado (mesmo conjunto suportado acima). |
| Indicators | `LongLength2` | Comprimento do suavizador lento comprado. |
| Indicators | `LongPhase2` | Parâmetro de fase para o suavizador lento comprado. |
| Indicators | `LongAppliedPrice` | Preço aplicado usado para construir o indicador comprado (fechamento, abertura, mediana, típico, ponderado, simples, quarto, trend-follow ou Demark). |
| Indicators | `ShortMethod1` | Método de suavização para a linha rápida vendida. |
| Indicators | `ShortLength1` | Comprimento do suavizador rápido vendido. |
| Indicators | `ShortPhase1` | Parâmetro de fase para o suavizador rápido vendido. |
| Indicators | `ShortMethod2` | Método de suavização para a linha lenta vendida. |
| Indicators | `ShortLength2` | Comprimento do suavizador lento vendido. |
| Indicators | `ShortPhase2` | Parâmetro de fase para o suavizador lento vendido. |
| Indicators | `ShortAppliedPrice` | Preço aplicado usado para construir o indicador vendido. |
| Risk | `LongStopLoss` | Distância de preço absoluta para o stop-loss comprado (0 desabilita a verificação). |
| Risk | `LongTakeProfit` | Distância de preço absoluta para o take-profit comprado (0 desabilita a verificação). |
| Risk | `ShortStopLoss` | Distância de preço absoluta para o stop-loss vendido (0 desabilita a verificação). |
| Risk | `ShortTakeProfit` | Distância de preço absoluta para o take-profit vendido (0 desabilita a verificação). |

## Notas de implementação
- O indicador XFatlXSatlCloud é implementado como um indicador de alto nível do StockSharp. Os componentes rápido e lento são produzidos aplicando os coeficientes de resposta ao impulso finito FATL/SATL originais seguidos de indicadores de suavização selecionados pelo usuário.
- Apenas médias móveis do StockSharp comumente disponíveis são expostas (`Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`, `ZeroLag`, `Kaufman`). Outras famílias de suavização MQL (como Parabolic ou T3) não estão incluídas.
- `LongSignalBar` e `ShortSignalBar` imitam o parâmetro original `SignalBar`. Um valor de 1 significa "usar a barra concluída anterior" ao detectar o cruzamento.
- Os offsets de stop-loss e take-profit esperam distâncias de preço absolutas. São aplicados usando o máximo/mínimo da vela relativo ao preço de entrada registrado e não dependem de valores de ponto específicos do broker.

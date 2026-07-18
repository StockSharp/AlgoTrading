# Estratégia Exp Sar Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port de alto nível do StockSharp do consultor especialista **Exp_Sar_Tm_Plus**. A estratégia monitora reversões do Parabolic SAR em um período configurável e replica as funcionalidades originais de gerenciamento de dinheiro e tempo limite enquanto mantém a lógica compatível com a API de alto nível do StockSharp.

## Lógica de trading

- Os candles são assinados a partir do parâmetro `CandleType` (padrão: período de 4 horas). O indicador Parabolic SAR é calculado com os coeficientes `SarStep` e `SarMaximum` definidos pelo usuário.
- Para cada candle finalizado o algoritmo armazena preços de fechamento e valores de SAR. O parâmetro `SignalBar` seleciona qual candle fechado é avaliado (padrão: a última barra fechada) e compara com o candle anterior para detectar uma mudança na direção do SAR.
- Uma posição **comprada** é aberta quando o preço cruza **acima** do SAR (candle anterior abaixo do SAR, candle selecionado acima do SAR) e o trading comprado está habilitado. A exposição vendida existente é fechada automaticamente antes de mudar de direção.
- Uma posição **vendida** é aberta quando o preço cruza **abaixo** do SAR (candle anterior acima do SAR, candle selecionado abaixo do SAR) e o trading vendido está habilitado. A exposição comprada existente é liquidada primeiro.
- As posições são fechadas quando o SAR se move contra elas (`AllowLongExit` / `AllowShortExit`), quando os níveis opcionais de stop-loss / take-profit são violados, ou quando o tempo máximo de manutenção (`UseTimeExit` + `HoldingMinutes`) expira.
- Os níveis de stop-loss e take-profit são recalculados em cada entrada usando o `PriceStep` do instrumento. Ambos os níveis são opcionais e ignorados quando o valor correspondente é zero.

## Parâmetros

- `MoneyManagement` – fração do `Volume` base que será negociado em cada entrada. Valores ≤ 0 retornam ao valor `Volume` simples. Normalizado para o `VolumeStep` do instrumento.
- `ManagementMode` – enumeração preservada do especialista original. Todos os modos atualmente se comportam como `Lot` (volume fixo) dentro deste port.
- `StopLossPoints` / `TakeProfitPoints` – distância em passos de preço usada para definir níveis de proteção em torno do preço de entrada. Definir como zero para desabilitar.
- `DeviationPoints` – configuração original de slippage. Mantido por completude, mas a API de alto nível executa ordens a mercado sem usar este valor.
- `AllowLongEntry`, `AllowShortEntry` – interruptores para abertura de posições compradas/vendidas.
- `AllowLongExit`, `AllowShortExit` – interruptores para fechamento de posições quando o preço cruza o SAR na direção oposta.
- `UseTimeExit` – habilita a liquidação de posição após `HoldingMinutes` minutos no mercado.
- `HoldingMinutes` – duração para a janela de saída baseada em tempo.
- `CandleType` – tipo de dados de candles para análise SAR.
- `SarStep`, `SarMaximum` – configuração do Parabolic SAR.
- `SignalBar` – número de candles fechados para deslocar a avaliação do sinal (0 = candle finalizado atual, 1 = anterior, etc.).

## Gerenciamento de risco e notas

- A estratégia invoca `StartProtection()` na inicialização, habilitando os serviços de proteção integrados do StockSharp.
- As saídas baseadas em tempo dependem do `CloseTime` do candle (fallback para `OpenTime` se não disponível) para medir o período de manutenção com precisão.
- Apenas uma posição líquida é mantida a qualquer momento. As reversões de posição fecham automaticamente o lado oposto antes de entrar em uma nova negociação.
- A implementação mantém o conjunto de parâmetros do especialista MQL5 original. Algumas opções (como modos de gerenciamento de dinheiro não-`Lot` ou `DeviationPoints` de ordens) são marcadores de posição porque a API de alto nível abstrai a mecânica do lado do broker.

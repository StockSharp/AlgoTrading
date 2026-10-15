# Estratégia de Cruzamento de Média Móvel Suavizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Cruzamento de Média Móvel Suavizada replica a lógica do Consultor Especialista MQL5 original **Smoothing Average (barabashkakvn's edition)**. Ela combina uma média móvel configurável com um filtro de distância de preço medido em pips. Quando o mercado se afasta suficientemente da média suavizada, a estratégia abre uma posição na direção do movimento (ou no lado oposto se o modo de reversão estiver habilitado). As posições são fechadas quando o preço reverte através de um canal expandido em torno da média móvel.

## Lógica de Negociação
### Modo padrão (`ReverseSignals = false`)
- **Entrada comprada:** o preço de fechamento sobe acima da média móvel menos `Entry Delta (pips)`.
- **Entrada vendida:** o preço de fechamento cai abaixo da média móvel mais `Entry Delta (pips)`.
- **Saída vendida:** o preço de fechamento sobe acima da média móvel mais `Entry Delta (pips) × Close Delta Coefficient`.
- **Saída comprada:** o preço de fechamento cai abaixo da média móvel menos `Entry Delta (pips) × Close Delta Coefficient`.

### Modo de reversão (`ReverseSignals = true`)
- **Entrada comprada:** o preço de fechamento cai abaixo da média móvel mais `Entry Delta (pips)`.
- **Entrada vendida:** o preço de fechamento sobe acima da média móvel menos `Entry Delta (pips)`.
- **Saída comprada:** o preço de fechamento cai abaixo da média móvel menos `Entry Delta (pips) × Close Delta Coefficient`.
- **Saída vendida:** o preço de fechamento sobe acima da média móvel mais `Entry Delta (pips) × Close Delta Coefficient`.

A média móvel pode ser deslocada para frente por vários candles. A estratégia emula esse comportamento mantendo um pequeno buffer dos valores mais recentes do indicador e usando o valor de `MaShift` barras atrás. Isso corresponde à linha deslocada produzida pela implementação original do MetaTrader.

## Parâmetros
- `Candle Type` – série de dados usada para os cálculos.
- `MA Length` – período da média de suavização.
- `MA Shift` – número de barras que a média móvel é deslocada para frente.
- `MA Type` – método de média móvel (simples, exponencial, suavizada ou ponderada linearmente).
- `Price Source` – preço de vela alimentado na média móvel (padrão: preço típico).
- `Entry Delta (pips)` – distância da média móvel necessária para acionar entradas. Convertida em preço usando o tamanho de pip do instrumento.
- `Close Delta Coefficient` – multiplicador aplicado ao delta de entrada ao verificar condições de saída.
- `Reverse Signals` – inverte a lógica de entrada comprada/vendida.
- `Trade Volume` – tamanho de ordem usado para entradas compradas e vendidas.

## Gerenciamento de Risco
- As ordens são enviadas com o parâmetro fixo `Trade Volume`. A estratégia não escala enquanto uma posição está aberta.
- Todas as saídas são baseadas em regras. Nenhuma ordem de stop-loss ou take-profit é enviada, mas `StartProtection()` é invocado para habilitar a rede de segurança no nível da plataforma.
- O modo de reversão está disponível para comportamento contra-tendência sem alterar outras configurações.

## Notas de Implementação
- O tamanho do pip é derivado de `Security.PriceStep`. Símbolos FX de três ou cinco dígitos recebem o mesmo ajuste de 10× que no código MQL5.
- A média móvel usa a seleção de `Price Source` para que preços típicos, medianos ou outros preços de vela possam ser correspondidos às configurações do EA original.
- As comparações de entrada e saída usam o fechamento da vela como proxy estável para as verificações bid/ask no Consultor Especialista de origem.
- Todos os comentários dentro do código C# são fornecidos em inglês, conforme exigido pelas diretrizes de conversão.

# Estratégia SV Rompimento Diário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia SV Rompimento Diário** é uma conversão direta em C# do assessor especialista do MetaTrader 5 "SV v.4.2.5". O sistema avalia a ação do preço uma vez por barra completa e permite no máximo uma operação por dia de bolsa. A negociação começa apenas após o horário de início configurado e depende da relação entre o intervalo recente de máxima/mínima e duas médias móveis suavizadas. Uma posição comprada é aberta quando o intervalo analisado completo permanece abaixo de ambas as médias, sinalizando um rebote antecipado de condições de sobrevenda. Por outro lado, uma posição vendida é aberta quando o intervalo permanece acima de ambas as médias, sinalizando uma possível reversão do território de sobrecompra.

## Regras de negociação
### Condições de entrada
- **Filtro diário** – nenhuma operação é avaliada até que o horário atual do servidor seja posterior a *Start Hour*/*Start Minute*. Apenas uma entrada é permitida por dia.
- **Janela de dados** – a estratégia ignora as `Shift` barras mais recentes e analisa as próximas `Interval` barras. Seus preços mais altos e mais baixos são comparados com as médias móveis deslocadas.
- **Entrada comprada** – se o preço mais alto na janela analisada estiver estritamente abaixo da MA lenta **e** o preço mais baixo estiver estritamente abaixo da MA rápida, entrar comprado (fechando primeiro qualquer posição vendida existente).
- **Entrada vendida** – se o preço mais baixo na janela analisada estiver estritamente acima da MA lenta **e** o preço mais alto estiver estritamente acima da MA rápida, entrar vendido (fechando primeiro qualquer posição comprada existente).

### Gestão de saída
- **Stop-loss inicial** – colocado a `Stop Loss (pips)` do preço de entrada. Se o nível for atingido, a posição é fechada.
- **Take-profit** – colocado a `Take Profit (pips)` do preço de entrada. Se o nível for atingido, a posição é fechada.
- **Trailing stop** – quando habilitado (tanto a distância de trailing quanto o passo são maiores que zero), o stop se move na direção do lucro. Para comprados, o stop é elevado para `Fechamento − Trailing Stop` quando o preço avança mais de `Trailing Stop + Trailing Step`; os vendidos espelham a lógica.
- **Bloqueio diário** – independentemente de como uma operação sai, a estratégia não abrirá uma nova posição até o próximo dia de negociação.

### Dimensionamento de posição
- **Modo manual** – quando *Use Manual Volume* é `true`, a estratégia envia o valor fixo de *Volume* (ajustado ao passo de volume do instrumento).
- **Modo baseado em risco** – quando *Use Manual Volume* é `false`, a estratégia estima o tamanho da operação a partir do capital da conta e do `Risk %`. Divide o capital em risco pelo valor monetário da distância de stop configurada, usando informações do passo do instrumento quando disponível.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| Use Manual Volume | `false` | Usar o valor fixo de `Volume` em vez do dimensionamento baseado em risco. |
| Volume | `0.1` | Volume de negociação quando o dimensionamento manual está habilitado. |
| Risk % | `5` | Percentagem do capital da conta arriscado por operação quando o dimensionamento manual está ativo. |
| Stop Loss (pips) | `50` | Distância do stop-loss em pips. Defina como `0` para desabilitar. |
| Take Profit (pips) | `50` | Distância do take-profit em pips. Defina como `0` para desabilitar. |
| Trailing Stop (pips) | `5` | Distância do trailing stop em pips. Requer que `Trailing Step` seja maior que zero. |
| Trailing Step (pips) | `5` | Incremento mínimo de lucro antes de o trailing stop ser movido. |
| Start Hour | `19` | Hora (horário da bolsa) quando as entradas podem começar. |
| Start Minute | `0` | Minuto (horário da bolsa) quando as entradas podem começar. |
| Shift | `6` | Número das barras mais recentes excluídas antes de analisar o intervalo. |
| Interval | `27` | Número de barras históricas usadas para calcular a janela de máxima/mínima. |
| Fast MA Period | `14` | Comprimento da média móvel rápida. |
| Fast MA Shift | `0` | Deslocamento horizontal (barras atrás) usado para o valor da MA rápida. |
| Fast MA Method | `Smma` | Método de média móvel para a MA rápida. |
| Fast Applied Price | `Median` | Fonte de preço para a MA rápida. |
| Slow MA Period | `41` | Comprimento da média móvel lenta. |
| Slow MA Shift | `0` | Deslocamento horizontal (barras atrás) usado para o valor da MA lenta. |
| Slow MA Method | `Smma` | Método de média móvel para a MA lenta. |
| Slow Applied Price | `Median` | Fonte de preço para a MA lenta. |
| Candle Type | `1 hour` | Série de velas usada para cálculos. |

## Notas adicionais
- A conversão mantém o comportamento original de analisar uma janela de preço atrasada (`Shift` + `Interval`) para evitar as barras mais recentes ao determinar rompimentos.
- A lógica de trailing usa o preço de fechamento da vela para aproximar as atualizações de trailing baseadas em ticks do MetaTrader. Ajuste as distâncias em pips se o seu instrumento requer precisão diferente.
- O dimensionamento baseado em risco depende de `Security.PriceStep`, `Security.StepPrice` e `Security.VolumeStep`. Forneça esses valores nas configurações do seu instrumento para um dimensionamento de lotes preciso.
- A estratégia chama `StartProtection()` para que você possa anexar regras de risco globais adicionais se necessário.
- Para espelhar o EA original, certifique-se de que o feed de dados e a conta de trading operem no mesmo fuso horário do servidor referenciado pelos parâmetros *Start Hour* e *Start Minute*.

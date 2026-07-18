# Alerta Boring EA2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Boring EA2 Alert recria a lógica de notificação do expert advisor `boring-ea2` do MetaTrader 4. A estratégia escuta candles concluídos, calcula três médias móveis simples (SMA 3, SMA 20, SMA 150) e emite logs informativos sempre que ocorre um cruzamento entre as médias móveis. A implementação evita intencionalmente colocar ordens: o objetivo é fornecer alertas oportunos que traders possam combinar com execução discricionária ou outras estratégias automatizadas.

## Lógica da estratégia
### Acompanhamento de médias móveis
* **Viés de curto prazo:** uma SMA de 3 períodos reage a mudanças imediatas de preço.
* **Tendência média:** uma SMA de 20 períodos suaviza o preço no horizonte de swing de curto prazo.
* **Tendência longa:** uma SMA de 150 períodos representa o pano de fundo dominante.

### Detecção de cruzamentos
* **SMA3 vs SMA20:** informa "crossed up" quando a SMA3 sobe acima da SMA20 e "crossed down" quando cai abaixo. Flags internas garantem que cada transição seja relatada uma vez.
* **SMA3 vs SMA150:** espelha a mesma lógica contra a média de longo prazo para detectar surtos de momentum ou reversões contra a tendência predominante.
* **SMA20 vs SMA150:** adiciona uma camada de confirmação de médio/longo prazo para que mudanças na estrutura de timeframe superior disparem seus próprios alertas.
* **Guarda de inicialização:** o primeiro candle concluído apenas semeia o estado inicial. Alertas começam no segundo candle concluído quando uma mudança real de relação é observada.

### Formato de notificação
* Alertas espelham a mensagem original do EA: `Alert!!! - SYMBOL - TF - description`.
* O código de timeframe é derivado do tipo de candle configurado. Rótulos padrão no estilo MetaTrader (M1, M5, H1 etc.) são usados quando disponíveis; outros timeframes usam notação compacta (por exemplo, `M45` ou `D2`).
* Mensagens são escritas com `AddInfoLog`, permitindo roteamento para visualizadores de log, scripts ou dashboards GUI.

## Parâmetros
* **Short SMA Length:** número de períodos da média móvel rápida (padrão `3`).
* **Medium SMA Length:** número de períodos da média móvel intermediária (padrão `20`).
* **Long SMA Length:** número de períodos da média móvel lenta (padrão `150`).
* **Candle Type:** timeframe usado para calcular as médias móveis. O padrão é candles de 1 minuto, correspondendo às checagens baseadas em ticks do EA com alta reatividade.

## Notas adicionais
* A estratégia não envia, modifica nem cancela ordens. Ela é puramente informativa.
* Como `Bind` alimenta valores finalizados, cada cruzamento é avaliado em candles concluídos. Isso evita as viradas intrabar ruidosas que o EA original mitigava contando ticks.
* Notificações baseadas em logging podem ser integradas a handlers personalizados assinando eventos de log da estratégia dentro de uma aplicação hospedeira.
* Nenhuma tradução Python é fornecida neste momento; apenas a versão C# está incluída no pacote API.

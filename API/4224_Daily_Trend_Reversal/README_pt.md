# Reversão diária de tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Daily Trend Reversal é uma versão do MetaTrader 4 consultor especialista `dailyTrendReversal_D1`. A estratégia ancora as negociações intradiárias na abertura, máxima e mínima do dia atual e só participa quando a ação do preço e o Commodity Channel Index (CCI) confirmam a mesma tendência direcional. A negociação é limitada a uma sessão GMT configurável, opcionalmente interrompida após atingir uma meta de lucro diário e pode sair das posições imediatamente quando os filtros mudam para o lado oposto.

## Lógica estratégica
### Filtros de polarização diários
* **Etapas direcionais** – a estratégia avalia até três condições para validar o viés diário:
  1. A distância do preço atual ao extremo diário deve exceder um limite de risco expresso em pips.
  2. A distância da abertura ao extremo oposto também deve exceder o limite de risco e o preço deve permanecer dentro de 10 pips da abertura diária.
  3. (Opcional) A vela atual deve fechar na direção do movimento enquanto o preço ainda estiver dentro de 10 pips da abertura diária.
* **Dominância de faixa** – compara a distância da abertura à máxima versus a abertura à mínima. O lado mais longo define a tendência ativa.
* **CCI tendência** – os últimos três valores CCI finalizados devem estar aumentando monotonicamente (para posições compradas) ou diminuindo (para posições vendidas).

### Regras de entrada
* **Entradas longas**
  * Permitido somente durante a janela de negociação GMT configurada em dias úteis.
  * O preço atual deve estar acima da abertura diária, as etapas direcionais devem confirmar uma tendência de alta, a dominância do intervalo deve favorecer a alta e a tendência CCI deve estar subindo.
  * Só abre uma posição longa se a posição líquida for plana ou curta (a exposição curta é fechada como parte da reversão para longa).
* **Entradas curtas**
  * Condições espelhadas: preço abaixo da abertura diária, passos direcionais confirmam uma tendência de baixa, o domínio do intervalo favorece o lado negativo e a tendência CCI está em declínio.
  * Abre apenas quando a posição líquida é plana ou longa.

### Regras de saída
* **Take Profit/Stop Loss fixos** – expresso em pips relativos à entrada. Um valor de `0` desativa o respectivo nível.
* **Controle de sessão e holding** – uma vez atingido o horário de fechamento GMT, ou decorrido o tempo de holding em horas, as posições lucrativas fecham imediatamente. As negociações perdedoras mudam para o modo de equilíbrio e fecham assim que o preço retorna à entrada.
* **Saída de reversão (opcional)** – se ativada, as posições compradas são fechadas quando os filtros de baixa se alinham (preço abaixo da abertura e tendências diárias/CCI apontando para baixo); os shorts são fechados simetricamente quando os filtros ascendentes se alinham.
* **Parada de lucro diário** – combina o lucro realizado desde a primeira negociação do dia com PnL flutuante. Quando o limite configurado é atingido, todas as posições são fechadas e novas entradas são suspensas até que o parâmetro seja reativado manualmente.

## Parâmetros
* **Auto Trading** – alterna se a estratégia pode abrir novas negociações.
* **Reversal Exit** – permite saídas imediatas quando a tendência diária oposta é confirmada.
* **Etapas da tendência** – seleciona quantos filtros de etapas (1–3) devem passar para validar a tendência diária.
* **Volume** – volume de pedidos para entradas no mercado.
* **Take Profit (pips)** – distância fixa da meta de lucro; defina como `0` para desativar.
* **Stop Loss (pips)** – distância de proteção do stop; defina como `0` para desativar.
* **Profit Stop** – meta de lucro em unidades de preço que pausa a negociação pelo resto do dia; `0` desativa o recurso.
* **GMT Diff** – hora do gráfico menos GMT (em horas). Usado para converter os limites da sessão GMT em tempo gráfico.
* **Hora Inicial / Hora Final** – Horário GMT que limita a janela de negociação para novas posições.
* **Hora de fechamento** – hora GMT após a qual a estratégia força saídas ou arma a lógica de ponto de equilíbrio.
* **Horário de espera** – período máximo de tempo que uma negociação pode permanecer aberta antes que a lógica da sessão seja acionada.
* **Risco (pips)** – distância pip utilizada pelas etapas direcionais.
* **CCI Período** – número de períodos do Commodity Channel Index.
* **Tipo de vela** – período de tempo que orienta os cálculos (padrão: velas de 15 minutos).

## Notas adicionais
* A estratégia detecta o tamanho do pip a partir da etapa de preço do título. Os símbolos FX de cinco e três dígitos convertem automaticamente as distâncias de pip configuradas em incrementos de preço.
* O rastreamento de lucro diário é redefinido com a primeira vela de cada novo dia de negociação, capturando o PnL realizado atual como a nova linha de base.
* Não há implementação Python para esta estratégia; apenas a versão C# é fornecida no pacote API.

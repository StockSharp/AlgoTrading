# Estratégia de crossover MA rápido e lento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de cruzamento Fast Slow MA** reproduz o comportamento do MetaTrader 4 consultor especialista original `_HPCS_FastSlowMACrosssover_MT4_EA_V01_WE`. A estratégia observa duas médias móveis exponenciais (EMAs) calculadas na série de velas selecionadas e emite negociações quando a média rápida cruza a lenta dentro de uma janela de negociação intradiária configurável. As saídas protetoras de take-profit e stop-loss são expressas em pips, portanto, o comportamento corresponde à implementação MQL que depende dos dígitos do corretor para escalar os preços.

## Lógica de negociação

1. Assine o tipo de vela configurado (padrão: velas de 1 minuto).
2. Calcule dois EMAs:
   - Período EMA rápido (padrão **14**).
   - Período EMA lenta (padrão **21**).
3. Avalie cada vela acabada:
   - Verifique se o tempo de fechamento da vela está dentro da janela de negociação permitida.
   - Detecte um **cruzamento de alta** quando o EMA rápido cruza acima do EMA lento.
   - Detecte um **cruzamento de baixa** quando o EMA rápido cruza abaixo do EMA lento.
4. Executar ordens:
   - Feche a exposição oposta se uma posição inversa estiver aberta.
   - Insira uma ordem de mercado com o volume configurado (parâmetro **Trade Volume**).
   - Armazene o preço de fechamento da vela como âncora de entrada para cálculos de risco.
5. Gerencie posições abertas usando máximos e mínimos de velas:
   - Feche uma posição longa se o preço se mover **Stop Loss (pips)** abaixo da entrada.
   - Feche uma posição longa se o preço subir **Take Profit (pips)** acima da entrada.
   - Aplicar a lógica simétrica para posições curtas (stop acima da entrada, objetivo abaixo da entrada).

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Período MA rápido** | Comprimento do EMA rápido usado para detecção de cruzamento. |
| **Período MA lento** | Comprimento da lentidão EMA. |
| **Take Profit (pips)** | Distância, em pips, usada para calcular as metas de lucro longas e curtas. |
| **Stop Loss (pips)** | Distância, em pips, usada para calcular os preços dos stop de proteção. |
| **Hora de início** | Início da janela diária de negociação (inclusive). |
| **Tempo de parada** | Fim da janela diária de negociação (inclusive). |
| **Tipo de vela** | Série de velas usadas para alimentar os indicadores. |
| **Volume comercial** | Volume de ordem de mercado para cada sinal. |

## Notas

- O tamanho do pip é derivado da etapa do preço do título e da precisão decimal. Quando o instrumento usa 5 ou 3 dígitos decimais, a estratégia multiplica a etapa de preço por **10** para corresponder ao cálculo do pip de MetaTrader.
- O filtro de tempo oferece suporte a sessões noturnas. Quando o **Horário de Início** for posterior ao **Hora de Parada**, a negociação permanecerá ativa até meia-noite e será retomada a partir da meia-noite até o horário de término.
- Apenas um sinal por vela é permitido, garantindo que o comportamento corresponda ao EA original que protegia contra múltiplos envios por barra.
- As ordens de saída protetoras são executadas pela lógica da estratégia em vez de ordens de repouso. Isso reflete a abordagem EA em que os níveis de stop loss e takeprofit foram definidos no envio do pedido.

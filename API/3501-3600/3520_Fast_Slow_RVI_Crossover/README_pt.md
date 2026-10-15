# Estratégia de crossover RVI rápido e lento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o consultor especialista MetaTrader `_HPCS_FastSlowRVIsCrossOver_MT4_EA_V01_WE`. Ele negocia quando a linha principal do Índice de Vigor Relativo (RVI) cruza sua linha de sinal durante a sessão de negociação configurada. Apenas uma negociação é permitida por vela, e a estratégia suporta stop loss opcional, takeprofit e distâncias de trailing stop expressas em pips.

## Lógica de negociação
1. Crie velas padrão baseadas em tempo selecionadas pelo parâmetro **Tipo de vela**.
2. Calcule o RVI com o **Período RVI** configurado e uma média móvel simples de 4 períodos como linha de sinal.
3. Quando o RVI subir acima da linha de sinal, feche qualquer posição curta e abra/escale para uma posição longa.
4. Quando o RVI cair abaixo da linha de sinal, feche qualquer posição longa e abra/escale para uma posição curta.
5. Ignore os sinais que aparecem fora do intervalo **Hora de Início** e **Hora de Parada**.
6. Emitir ordens de proteção de acordo com os parâmetros de risco selecionados. As paradas finais são gerenciadas pelo mecanismo de proteção StockSharp.
7. Evite entradas duplicadas na mesma vela reagindo apenas uma vez por barra.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| **Período RVI** | Número de barras utilizadas pelo Índice de Vigor Relativo. |
| **Take Profit (pips)** | Distância opcional de take-profit medida em pips. Defina como zero para desativar. |
| **Stop Loss (pips)** | Distância de stop-loss opcional medida em pips. Defina como zero para desativar. |
| **Trailing Stop (pips)** | Distância de parada móvel opcional em pips. Defina como zero para desativar o rastreamento. |
| **Etapa final (pips)** | Movimento favorável mínimo necessário antes que o trailing stop seja apertado. Funciona somente quando o trailing stop está ativo. |
| **Volume** | Volume de pedidos enviado em cada entrada. |
| **Tipo de vela** | Período de tempo ou tipo de dados de vela personalizado usado para análise. |
| **Hora de início** | Início da janela diária de negociação (inclusive). |
| **Tempo de parada** | Fim da janela diária de negociação (exclusivo). |

## Notas
- O tamanho do pip é adaptado ao tamanho do tick de segurança para corresponder ao tratamento de MetaTrader pontos (símbolos de 5 e 3 dígitos usam um multiplicador de 10×).
- Ligue para `StartProtection` uma vez dentro de `OnStarted` para ativar ordens de proteção e gerenciamento de rastreamento.
- Todos os comentários no código-fonte são escritos em inglês, conforme exigido pelas diretrizes do projeto.

# MACD Estratégia de cruzamento de sinal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Este exemplo converte o MetaTrader 4 consultor especialista original `MACD_v1.mq4` em uma estratégia de alto nível StockSharp. O algoritmo rastreia cruzamentos de divergência de convergência de média móvel (MACD) e negocia na direção da nova tendência. As saídas de proteção opcionais reproduzem o comportamento original do consultor: um stop-loss, um take-profit distante e uma meta de lucro parcial que liquida metade da posição atual.

## Lógica de negociação
1. **Fonte de dados** – a estratégia assina a série de velas configuradas (velas de 5 minutos por padrão) e vincula um indicador `MovingAverageConvergenceDivergenceSignal`.
2. **Condições de entrada**:
   - Digite **long** quando a linha MACD cruzar acima da linha de sinal. Se uma posição curta estiver ativa, ela será fechada antes de abrir a posição longa.
   - Digite **short** quando a linha MACD cruzar abaixo da linha de sinal. Se existir uma posição longa, ela será fechada primeiro.
3. **Condições de saída**:
   - O cruzamento oposto MACD fecha a posição atual e abre uma nova posição na direção oposta.
   - Um take-profit e stop-loss configuráveis gerenciados por `StartProtection` refletem os parâmetros TP/SL originais (medidos em pontos de instrumento).
   - Uma meta de lucro parcial fecha metade da posição aberta quando o preço avança em um valor especificado a partir do nível de entrada. A saída parcial é acionada apenas uma vez por posição.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| **Período rápido** | 23 | Comprimento EMA rápido para o cálculo MACD (espelha o parâmetro MQL `a = 2300`). |
| **Período lento** | 40 | Comprimento EMA lento para o cálculo MACD (`b = 4000` na origem). |
| **Período de Sinal** | 8 | Comprimento da linha de sinal (`c = 800` na fonte). |
| **Receba lucro** | 500 | Distância em faixas de preço para a ordem protetora de lucro. Defina como `0` para desativar. |
| **Stop Loss** | 80 | Distância em pontos de preço para a ordem de stop loss de proteção. Defina como `0` para desativar. |
| **Lucro Parcial** | 70 | Distância nos pontos de preço para fechar metade da posição aberta. Defina como `0` para desativar saídas parciais. |
| **Tipo de vela** | Período de 5 minutos | Série de velas usada para cálculos de indicadores.

## Notas
- Os parâmetros do indicador foram dimensionados para comprimentos MACD típicos (23/40/8) porque o script MQL os expressou como centésimos (2300/4000/800).
- A estratégia restaura automaticamente a bandeira de saída parcial sempre que uma nova posição é acumulada.
- Os auxiliares de desenho de gráfico destacam velas, valores MACD e as negociações da estratégia quando um painel de gráfico está disponível.
- A manipulação de volume depende da propriedade da estratégia base `Volume`. Ajuste-o antes de iniciar a estratégia para corresponder ao tamanho do seu instrumento.

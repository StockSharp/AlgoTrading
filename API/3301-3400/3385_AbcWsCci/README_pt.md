# Estratégia AbcWsCci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia AbcWsCci** combina dois padrões clássicos de reversão de velas japonesas — **Três Soldados Brancos** e **Três Corvos Negros** — com o indicador **Commodity Channel Index (CCI)** para confirmação. O sistema verifica as velas finalizadas, mede o tamanho do corpo em relação a uma linha de base da média móvel e abre negociações apenas quando o forte impulso de várias velas se alinha com os extremos CCI. As saídas de posição são acionadas quando o CCI sai das zonas extremas, sinalizando que o impulso está diminuindo.

## Lógica de negociação
- Mantenha uma média móvel dos tamanhos dos corpos das velas para qualificar velas “longas”.
- Detecte o padrão dos Três Soldados Brancos (três fortes velas de alta consecutivas com pontos médios ascendentes).
- Detecte o padrão dos Três Corvos Negros (três fortes velas de baixa consecutivas com pontos médios decrescentes).
- Confirme as entradas de alta com CCI caindo abaixo de **-50** e as entradas de baixa com CCI subindo acima de **50**.
- Feche as posições longas quando CCI ultrapassar os níveis **-80** ou **80** e feche as posições curtas nas condições espelhadas.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CciPeriod` | Comprimento do indicador CCI usado para confirmação. | 37 |
| `BodyAveragePeriod` | Número de velas na média móvel que define o tamanho mínimo do corpo “forte”. | 13 |
| `CandleType` | Período de vela usado para detecção de padrões. | 1 hora |

## Indicadores
- **Commodity Channel Index (CCI)**: avalia extremos de momentum para sinais de confirmação e saída.
- **Média Móvel Simples dos corpos das velas**: Estabelece o tamanho mínimo da vela necessário para um padrão válido.

## Gerenciamento de posição
- Digite **long** quando três soldados brancos se formarem e CCI estiver abaixo de -50 enquanto nenhuma posição longa estiver ativa.
- Insira **short** quando Três Corvos Negros se formarem e CCI estiver acima de 50 enquanto nenhuma posição curta estiver ativa.
- Saia das posições **longas** quando CCI sair da banda -80/80, indicando que o impulso de alta se esgotou.
- Saia das posições **curtas** quando CCI sair da banda +80/-80, sinalizando perda de impulso de baixa.

## Notas de uso
- A estratégia é orientada por eventos: apenas velas totalmente concluídas são processadas.
- Funciona melhor em instrumentos de tendência onde o impulso de múltiplas velas combinado com os extremos do oscilador fornece sinais confiáveis.
- Considere combinar com regras adicionais de gestão de risco (stop-loss, dimensionamento de posição) dependendo do seu ambiente de negociação.

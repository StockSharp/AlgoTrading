# Estratégia de Rompimento de Momentum Ancorado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Rompimento de Momentum Ancorado usa a razão entre uma média móvel exponencial (EMA) e uma média móvel simples (SMA) para medir o momentum. Quando a EMA de curto prazo começa a subir mais rápido do que a SMA de longo prazo, indica momentum altista crescente. Por outro lado, uma razão em queda sinaliza fortalecimento do momentum baixista.

## Como Funciona
1. **Indicadores**
   - EMA com período configurável.
   - SMA com período configurável.
2. **Cálculo do Momentum**
   - `Momentum = 100 * (EMA / SMA - 1)`
   - Momentum positivo significa que a EMA está acima da SMA; momentum negativo significa que a EMA está abaixo da SMA.
3. **Lógica de Trading**
   - Se o momentum esteve diminuindo e então vira para cima, a estratégia entra em uma posição comprada.
   - Se o momentum esteve aumentando e então vira para baixo, a estratégia entra em uma posição vendida.
   - O tamanho da posição inclui automaticamente a posição existente para reverter quando necessário.
4. **Gestão de Risco**
   - Os níveis de stop-loss e take-profit são definidos como porcentagens do preço de entrada usando o mecanismo de proteção integrado.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `SmaPeriod` | Período para o indicador SMA. |
| `EmaPeriod` | Período para o indicador EMA. |
| `StopLossPercent` | Percentagem para o stop-loss. |
| `TakeProfitPercent` | Percentagem para o take-profit. |
| `CandleType` | Período de velas usado para os cálculos. |

## Notas
- A estratégia trabalha apenas com velas concluídas.
- Todas as ações de trading são executadas usando ordens a mercado.
- Os valores dos indicadores são obtidos através da API de alto nível `Bind` sem acessar diretamente os buffers históricos.

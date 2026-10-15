# Estratégia SlopeDirectionLine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o comportamento do Expert Advisor *Slope Direction Line*. Ela analisa a inclinação de uma linha de regressão linear construída sobre preços de fechamento. Uma posição comprada é aberta quando a inclinação de regressão se torna positiva após ser negativa, enquanto uma posição vendida é aberta quando ela se torna negativa após ser positiva. Posições opostas são fechadas a cada mudança de direção. Percentuais opcionais de stop-loss e take-profit protegem as posições através do mecanismo integrado `StartProtection`.

## Detalhes
- **Indicador** – `LinearRegression` do StockSharp. A estratégia usa o componente `LinearRegSlope` como sinal.
- **Sinal** – cruzamento da inclinação por zero. Uma inclinação positiva indica uma tendência de alta; uma inclinação negativa sinaliza uma tendência de baixa.
- **Entrada/Saída** – quando a inclinação muda de sinal, a posição atual é fechada e, se permitido, uma nova posição na direção da inclinação é aberta.
- **Controle de risco** – `StartProtection` é configurado com percentuais de take-profit e stop-loss definidos pelo usuário.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `CandleType` | Período de tempo usado para construir velas. |
| `Length` | Número de barras usadas no cálculo de regressão linear. |
| `TakeProfitPercent` | Distância percentual ao take-profit a partir do preço de entrada. |
| `StopLossPercent` | Distância percentual ao stop-loss a partir do preço de entrada. |
| `AllowLong` | Permitir abertura de posições compradas. |
| `AllowShort` | Permitir abertura de posições vendidas. |

## Uso
1. Adicionar a estratégia a uma aplicação StockSharp.
2. Configurar os parâmetros de acordo com o período desejado e o risco.
3. Iniciar a estratégia e monitorar as operações no gráfico.

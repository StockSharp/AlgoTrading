# Estratégia ColorJjrsxTimePlus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do especialista MetaTrader5 `Exp_ColorJJRSX_Tm_Plus`. A estratégia opera reversões de tendência detectadas com um oscilador RSI suavizado por Jurik e inclui saídas opcionais baseadas em tempo, imitando os toggles de gestão monetária originais.

## Visão geral

- **Ideia**: Rastrear a inclinação do oscilador Color JJRSX (aproximado via RSI suavizado por uma Média Móvel Jurik). Quando o oscilador sobe, o sistema pode fechar vendidos e opcionalmente abrir comprados, e vice-versa para quedas.
- **Mercado**: Instrumento único definido pelo `Security` conectado.
- **Período**: Configurável; padrão são velas de 4 horas (correspondendo à entrada EA original).
- **Direção**: Comprado e vendido. Cada direção pode ser desabilitada independentemente.
- **Tipo de ordem**: Ordens a mercado via `BuyMarket()` / `SellMarket()`.

## Pilha de indicadores

1. **Relative Strength Index (RSI)** — oscilador de momentum base usando o parâmetro `RSI Length` (reflete `JurXPeriod`).
2. **Jurik Moving Average (JMA)** — suaviza a saída do RSI com `Smoothing Length` (reflete `JMAPeriod`). O parâmetro de fase JMA da versão MQL não está exposto pelo StockSharp e portanto é omitido.
3. **Signal Shift** — reproduz o parâmetro `SignalBar`. Os sinais são gerados a partir do valor `Signal Shift` barras atrás e dos dois valores precedentes para detectar mudanças de inclinação.

## Lógica de trading

### Gestão de comprados
- **Entrada**: Habilitada por `Enable Long Entries`. Requer que o oscilador suavizado estivesse declinando duas barras atrás (`previous > older` é falso), girou para cima na última barra completa (`previous < older`), e continua mais alto na barra atual (`current > previous`). A posição deve estar plana ou vendida.
- **Saída**: Se `Exit Long on Downturn` estiver habilitado e o oscilador inclinar para baixo (`previous > older`), qualquer comprado aberto é fechado.

### Gestão de vendidos
- **Entrada**: Habilitada por `Enable Short Entries`. Requer que o oscilador gire para baixo (`previous > older`) e continue caindo na barra atual (`current < previous`) enquanto a estratégia está plana ou comprada.
- **Saída**: Se `Exit Short on Upturn` estiver habilitado e o oscilador inclinar para cima (`previous < older`), qualquer vendido aberto é coberto.

### Filtro de tempo
- `Enable Time Exit` fecha posições assim que seu tempo de manutenção excede `Holding Minutes`. Isso reflete o temporizador do especialista original que liquida posições após `nTime` minutos.

### Controles de risco
- `Stop Loss (pts)` e `Take Profit (pts)` são convertidos em níveis de proteção do StockSharp via `StartProtection` usando `UnitTypes.PriceStep`.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Indicator Timeframe` | Tipo de vela para os cálculos do indicador. | Velas de 4 horas |
| `RSI Length` | Período para o RSI (análogo ao período JurX). | 8 |
| `Smoothing Length` | Comprimento do suavizado Jurik MA (análogo ao período JMA). | 3 |
| `Signal Shift` | Número de barras completadas a pular antes de verificar inclinações (`SignalBar`). | 1 |
| `Enable Long Entries` / `Enable Short Entries` | Permitir abrir operações em cada direção. | true |
| `Exit Long on Downturn` / `Exit Short on Upturn` | Permitir saídas impulsionadas pelo oscilador para posições existentes. | true |
| `Enable Time Exit` | Ativar a liquidação baseada em tempo de manutenção. | true |
| `Holding Minutes` | Minutos máximos para manter uma posição aberta. | 240 |
| `Stop Loss (pts)` | Distância do stop de proteção em passos de preço. | 1000 |
| `Take Profit (pts)` | Distância do alvo de lucro em passos de preço. | 2000 |

## Notas sobre a conversão

- O buffer do histograma JJRSX do indicador original é emulado com RSI + suavização Jurik. Apenas informações de inclinação são usadas, portanto as diferenças de escala numérica não afetam as decisões.
- As opções de gestão monetária (`MM`, `MMMode`, `Deviation`) não estão portadas. O dimensionamento de ordens no StockSharp deve ser tratado através da propriedade `Strategy.Volume` ou configurações de portfólio externas.
- As variáveis globais usadas no MQL para limitar a taxa de ordens são desnecessárias aqui porque a estratégia reage apenas a velas finalizadas.
- Todos os comentários e documentação estão em inglês conforme as diretrizes do repositório.

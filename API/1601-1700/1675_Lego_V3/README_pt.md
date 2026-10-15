# Estratégia Lego V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem do consultor especialista MQL4 "Lego_v3".  
Combina vários indicadores clássicos para gerar entradas e saídas:

- **Médias Móveis** – SMA rápida e lenta para detectar a direção da tendência.
- **Oscilador Stochastic** – os valores de %K e %D definem as zonas de sobrevenda e sobrecompra.
- **Awesome Oscillator** – confirma o alinhamento do momentum com a tendência.
- **Average True Range** – determina as distâncias de stop-loss e take-profit.

Uma posição comprada é aberta quando a MA rápida cruza acima da MA lenta, o Stochastic %K está abaixo do nível de compra e o Awesome Oscillator é positivo.  
Posições vendidas ocorrem em condições opostas. O ATR é usado uma vez no início para iniciar o gerenciamento do stop de proteção.

## Parâmetros

- `FastMaPeriod` – período para a média móvel rápida.
- `SlowMaPeriod` – período para a média móvel lenta.
- `StochK` – período de %K para o oscilador Stochastic.
- `StochD` – período de %D para o oscilador Stochastic.
- `StochBuy` – limiar da zona de compra para %K.
- `StochSell` – limiar da zona de venda para %K.
- `AtrPeriod` – período para o cálculo do ATR.
- `AtrMultiplier` – multiplicador aplicado ao ATR para os níveis de stop.
- `CandleType` – período das velas processadas.

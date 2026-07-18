# Estratégia BandsPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação do especialista **i-BandsPrice** do MetaTrader. Utiliza Bandas de Bollinger para medir a posição relativa do preço dentro do canal e reage quando o valor sai das zonas extremas.

## Lógica

1. Construir Bandas de Bollinger com período e desvio configuráveis.
2. Calcular a posição do preço dentro da banda como um valor entre -50 e +50.
3. Suavizar o valor com uma média móvel simples.
4. Gerar um código de cor:
   - `4` quando o valor suavizado está acima do nível superior.
   - `0` quando o valor suavizado está abaixo do nível inferior.
   - Outros números representam zonas intermediárias.
5. Uma posição comprada é aberta quando o indicador sai da zona superior (`4` → não `4`).
6. Uma posição vendida é aberta quando o indicador sai da zona inferior (`0` → positivo).
7. Posições compradas são fechadas quando o valor se torna não positivo.
8. Posições vendidas são fechadas quando o valor se torna não negativo.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| **BuyOpen** | Ativar entradas compradas. |
| **SellOpen** | Ativar entradas vendidas. |
| **BuyClose** | Ativar fechamento de posições compradas. |
| **SellClose** | Ativar fechamento de posições vendidas. |
| **BandsPeriod** | Período das Bandas de Bollinger. |
| **BandsDeviation** | Desvio para as bandas. |
| **Smooth** | Comprimento de suavização para o valor interno. |
| **UpLevel** | Limiar superior, padrão `25`. |
| **DnLevel** | Limiar inferior, padrão `-25`. |
| **CandleType** | Período de velas usado para os cálculos. |

## Notas

Esta estratégia demonstra como migrar a lógica baseada em indicadores do MetaTrader para o StockSharp usando a API de alto nível com `SubscribeCandles` e `Bind`.

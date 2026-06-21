# Estratégia DCA de Reversão à Média com Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra um valor fixo em dólares quando o preço cruza abaixo da banda inferior de Bollinger ou no primeiro dia de cada mês. Todas as posições são fechadas em uma data especificada.

## Parâmetros
- `InvestmentAmount` - valor investido a cada vez
- `OpenDate` - data de início das compras
- `CloseDate` - data para fechar todas as posições
- `StrategyMode` - reversão à média BB, DCA mensal ou combinado
- `BollingerPeriod` - período das Bollinger Bands
- `BollingerMultiplier` - multiplicador de desvio padrão
- `CandleType` - período para o cálculo de Bollinger

## Indicadores
- Bollinger Bands

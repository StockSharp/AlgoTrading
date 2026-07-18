# Estratégia Fibo Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Fibo Stop desloca o stop de proteção ao longo dos níveis de retração de Fibonacci definidos por dois preços: início e fim. A estratégia abre uma posição na direção do nível de início ao nível de fim e move o stop para cada novo nível de Fibonacci assim que o preço o cruza.

## Algoritmo
1. Determinar a direção do preço de início ao preço de fim. Se o fim for maior que o início, uma posição comprada é aberta; caso contrário, uma posição vendida.
2. Calcular os níveis de Fibonacci: 0%, 23.6%, 38.6%, 50%, 61.8%, 78.6%, 100%, 127% com base no intervalo.
3. O stop inicial é colocado atrás do nível de início usando o deslocamento especificado em passos de preço.
4. À medida que o preço de mercado se move e cruza o próximo nível de Fibonacci, o stop é movido para esse nível menos/mais o deslocamento.
5. A posição é fechada quando o preço atinge o trailing stop.

## Parâmetros
- `FiboStart` – preço base onde o cálculo de Fibonacci começa.
- `FiboEnd` – preço final que define o intervalo de Fibonacci.
- `OffsetPoints` – número de passos de preço adicionados atrás de cada nível de Fibonacci para colocar o stop.
- `CandleType` – série de velas utilizada para monitorar o preço.

## Observações
A estratégia usa apenas velas completas e não depende do histórico de valores de indicadores. Destina-se como exemplo de gerenciamento de um trailing stop com a API de alto nível do StockSharp.

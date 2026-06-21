# Laguerre CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina o filtro Laguerre, o Índice de Canal de Commodities (CCI) e uma média móvel exponencial.

## Visão geral
- O filtro Laguerre destaca extremos de sobrecompra e sobrevenda em uma escala de 0-1.
- O CCI confirma o momentum na mesma direção.
- A inclinação da EMA filtra operações para a tendência predominante.

## Regras de entrada
- **Comprado** quando o valor do Laguerre é 0, a EMA está subindo e o CCI está abaixo do limiar negativo `CciLevel`.
- **Vendido** quando o valor do Laguerre é 1, a EMA está caindo e o CCI está acima do limiar positivo `CciLevel`.

## Regras de saída
- Encerrar posições compradas quando o Laguerre exceder 0.9.
- Encerrar posições vendidas quando o Laguerre cair abaixo de 0.1.

## Parâmetros
- `LagGamma` – valor gama para o filtro Laguerre.
- `CciPeriod` – período para o cálculo do CCI.
- `CciLevel` – nível absoluto do CCI usado para entradas.
- `MaPeriod` – período para a média móvel.
- `TakeProfit` – take profit em unidades de preço absolutas (opcional).
- `StopLoss` – stop loss em unidades de preço absolutas (opcional).
- `CandleType` – tipo de vela usado para os indicadores.

A estratégia processa apenas velas finalizadas e usa os vínculos de API de alto nível do StockSharp para indicadores.

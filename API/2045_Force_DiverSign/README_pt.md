# Estratégia Force DiverSign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Force DiverSign opera com base em sinais de divergência entre dois indicadores Force Index calculados com períodos de suavização diferentes.
Ela procura um padrão de reversão de três candles juntamente com oscilações opostas nos valores rápido e lento do Force. Quando aparece uma divergência de alta,
a estratégia compra; quando aparece uma divergência de baixa, vende.

## Parâmetros
- `Period1` – período para o Force Index rápido.
- `Period2` – período para o Force Index lento.
- `MaType1` – tipo de média móvel usada para suavizar o Force Index rápido.
- `MaType2` – tipo de média móvel usada para suavizar o Force Index lento.
- `CandleType` – período dos candles para os cálculos.

## Lógica de operação
1. Calcular o Force Index bruto como o volume multiplicado pela variação do preço de fechamento.
2. Suavizar o valor bruto com duas médias móveis para obter as séries Force rápida e lenta.
3. Armazenar os últimos cinco valores de Force e os últimos quatro candles.
4. **Comprar** quando:
   - Os três candles anteriores formam um padrão baixo–alto–baixo.
   - Ambas as séries Force formam um mínimo local e depois sobem.
   - O Force rápido e o lento se movem em direções opostas entre o primeiro e o terceiro candle.
5. **Vender** quando:
   - Os três candles anteriores formam um padrão alto–baixo–alto.
   - Ambas as séries Force formam um máximo local e depois caem.
   - O Force rápido e o lento se movem em direções opostas entre o primeiro e o terceiro candle.

As posições são revertidas em cada sinal: uma compra fecha um vendido existente e uma venda fecha um comprado.

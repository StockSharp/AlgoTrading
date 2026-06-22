# Estratégia Loco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o indicador "Loco" originalmente escrito em MQL5. O indicador analisa os preços das velas e atribui uma cor (verde ou magenta). Uma mudança de cor sinaliza uma reversão de tendência.

## Lógica
- O indicador calcula uma série usando um preço configurável (fechamento por padrão) e um comprimento de retrocesso.
- Quando a cor muda de magenta para verde, a estratégia fecha qualquer posição vendida e abre uma posição comprada.
- Quando a cor muda de verde para magenta, a estratégia fecha qualquer posição comprada e abre uma posição vendida.

## Parâmetros
- **Candle Type** – tipo de velas utilizadas na estratégia.
- **Length** – número de barras para comparar o preço.
- **Price Type** – preço utilizado no cálculo do indicador.

## Notas
A estratégia usa uma implementação personalizada do indicador Loco. A versão em Python não está disponível.

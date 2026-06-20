# Estratégia Arpit Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento de Bollinger Band que aguarda um fechamento fora das bandas há duas velas atrás e entra quando o preço rompe o extremo daquela barra.

- **Indicadores**: Bollinger Bands (EMA 20, desvio 1.5)
- **Entrada**: Comprado quando o preço fechou abaixo da banda inferior há duas barras e o máximo atual supera o máximo daquela barra. Vendido quando o preço fechou acima da banda superior há duas barras e o mínimo atual cai abaixo do mínimo daquela barra.
- **Stops**: Stop colocado além do intervalo da vela atual com um buffer de 5% e take profit baseado em uma relação risco‑retorno.


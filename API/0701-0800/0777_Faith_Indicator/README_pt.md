# Estratégia de Indicador Faith
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia avalia a "fé" do mercado medindo a expansão do volume quando o preço atinge máximas mais altas ou mínimas mais baixas. Uma classificação positiva sugere que os compradores dominam, enquanto uma classificação negativa indica que os vendedores prevalecem. A estratégia opera em transições entre classificações positivas e negativas.

## Detalhes

- **Critérios de entrada:** a classificação Faith cruza acima de zero → comprar; cruza abaixo de zero → vender.
- **Comprado/Vendido:** ambos.
- **Critérios de saída:** sinal oposto.
- **Indicadores:** Highest, SMA.

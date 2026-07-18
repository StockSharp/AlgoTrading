# Estratégia ColorMETRO XRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma implementação em StockSharp inspirada no Expert Advisor original do MQL5 "Exp_ColorMETRO_XRSX". Utiliza duas médias móveis suavizadas para detectar mudanças de tendência. Uma posição comprada é aberta quando a média rápida cruza acima da média lenta, enquanto uma posição vendida é aberta quando a média rápida cruza abaixo da média lenta.

## Parâmetros

- **Fast Period** – período da média móvel rápida.
- **Slow Period** – período da média móvel lenta.
- **Candle Type** – período dos candles utilizados para os cálculos.

## Como Funciona

1. A estratégia subscreve a série de candles selecionada.
2. Dois indicadores `Sma` com períodos diferentes são calculados sobre o preço de fechamento.
3. Quando o SMA rápido cruza acima do SMA lento, qualquer posição vendida é fechada e uma posição comprada é aberta.
4. Quando o SMA rápido cruza abaixo do SMA lento, qualquer posição comprada é fechada e uma posição vendida é aberta.
5. Os valores anteriores das médias são armazenados para detectar cruzamentos apenas uma vez.

## Observações

- A estratégia utiliza a API de alto nível com `Bind` para o processamento de indicadores.
- `StartProtection` está habilitado para gerenciar os mecanismos de proteção.
- Esta implementação é uma tradução simplificada e não utiliza o indicador personalizado original.

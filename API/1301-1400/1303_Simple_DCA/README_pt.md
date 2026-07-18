# Estratégia DCA Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca uma ordem base e adiciona ordens de segurança quando o preço se desvia em uma porcentagem especificada. Sai quando o preço atinge um take profit calculado a partir do preço médio de entrada. O tamanho de cada ordem de segurança é multiplicado por um fator.

## Parâmetros
- Tipo de vela
- Tamanho da ordem base (moeda de cotação)
- Desvio de preço para a ordem de segurança (%)
- Máximo de ordens de segurança
- Take profit (%)
- Multiplicador do tamanho da ordem

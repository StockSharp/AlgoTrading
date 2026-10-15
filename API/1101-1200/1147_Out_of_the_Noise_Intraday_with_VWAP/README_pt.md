# Estratégia Intradiária "Out of the Noise" com VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa a abordagem de rompimento intradiário "Out of the Noise". A estratégia constrói limites dinâmicos superiores e inferiores ao redor da abertura da sessão usando movimentos absolutos médios dos últimos *Period* dias.

Posições compradas são abertas quando o preço rompe acima do limite superior, enquanto posições vendidas são abertas abaixo do limite inferior. Posições existentes saem em um cruzamento com o VWAP ou ao tocar o limite oposto. O tamanho da posição pode escalar opcionalmente para um alvo de volatilidade derivado do desvio padrão diário.

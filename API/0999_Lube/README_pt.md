# Estratégia LUBE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia mede o "atrito" ao redor do preço de fechamento atual, escaneando velas anteriores. Um filtro FIR define a direção da tendência.

- **Comprado** quando o atrito cai abaixo do nível de gatilho e a tendência está em alta.
- **Vendido** quando o atrito cai abaixo do nível de gatilho e a tendência está em baixa.
- **Saída** quando o atrito sobe acima do nível médio ou aparece o sinal contrário.

## Detalhes
- **Indicadores**: cálculo de atrito personalizado, filtro FIR.
- **Período**: velas de 30m por padrão.
- **Ambos os lados**: sim, vendidos opcionais.

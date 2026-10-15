# Estratégia de Regressão Logística com Machine Learning
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia treina novamente um modelo simples de regressão logística a cada barra.
O modelo utiliza os preços de fechamento recentes e uma série sintética derivada deles.
Se a probabilidade prevista de crescimento for superior a 0.5, a estratégia entra em posição comprada; caso contrário, fica vendida.
As posições são mantidas por um número fixo de barras.

## Detalhes
- **Entrada**: previsão > 0.5 → comprado, caso contrário vendido.
- **Saída**: sinal oposto ou período de manutenção atingido.
- **Comprado/Vendido**: ambos.
- **Período**: configurável, padrão 1 minuto.

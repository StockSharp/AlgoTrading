# Estratégia de Preço Destendenciado TRAX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza os osciladores TRAX e DPO para operar reversões de tendência.

## Detalhes
- **Critérios de entrada**: DPO cruzando TRAX com sinal TRAX e filtro SMA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinais de cruzamento opostos.
- **Stops**: Nenhum.
- **Valores padrão**: Comprimento TRAX 12, comprimento DPO 19, comprimento SMA de confirmação 3.
- **Filtros**: Sinal TRAX e SMA de confirmação.

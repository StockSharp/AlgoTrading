# Estratégia de Classificações Técnicas em Ativos Multi-Temporais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia agrega classificações técnicas de múltiplos períodos.
Compara o preço com uma média móvel e limites de RSI em velas de 1h, 4h e diárias.
Uma posição comprada é aberta quando a classificação combinada é positiva e uma vendida quando é negativa.

## Detalhes

- **Entrada**: Comprar quando a classificação média > 0; vender quando a classificação média < 0.
- **Indicadores**: SMA, RSI.
- **Períodos**: 1h, 4h, 1d.
- **Tipo**: Seguidor de tendência.
- **Stops**: Nenhum.
- **Direção**: Comprado e Vendido.
- **Risco**: Médio.
- **Complexidade**: Médio.

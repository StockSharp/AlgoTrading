# Estratégia RSI Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia RSI Adaptativo deriva um coeficiente de suavização do Índice de Força Relativa. Quando o RSI se desvia do nível neutro de 50, o coeficiente aumenta, fazendo com que o RSI adaptativo siga o preço mais de perto. Perto de 50, o coeficiente diminui e a curva se suaviza. Uma posição comprada é aberta quando o RSI adaptativo vira para cima, enquanto uma posição vendida é aberta quando ele vira para baixo.

## Detalhes

- **Critérios de entrada**:
  - O RSI adaptativo cruza acima do seu valor anterior.
  - O RSI adaptativo cruza abaixo do seu valor anterior.
- **Comprado/Vendido**: Operações compradas e vendidas.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 14
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

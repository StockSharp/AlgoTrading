# Estratégia de Entrada do Ouro Baseada em Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando duas barras de volume altistas consecutivas excedem a média móvel de volume. A segunda barra também deve ter volume maior que a primeira. Um alvo de lucro fixo fecha a posição assim que o preço se move um valor predefinido a favor.

## Detalhes

- **Critérios de entrada**:
  - Duas barras de volume altistas acima da média móvel de volume com volume crescente.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Alvo de lucro fixo em `entry price + Target Move`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Volume MA Period` = 20.
  - `Target Move` = 5.
- **Filtros**:
  - Categoria: Volume
  - Direção: Comprado
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

# Estratégia de Emulação Renko sem Repintura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Emula tijolos Renko usando preços de fechamento e opera em transições de padrão sem repintura.

## Detalhes

- **Critérios de entrada**:
  - Após a formação de um novo tijolo, comprar quando a direção do tijolo anterior e a sequência de preços mostram continuação de alta.
  - Vender na sequência inversa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Fechar posições quando a direção do tijolo se inverte.
- **Stops**: Não.
- **Valores padrão**:
  - `BrickSize` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia SMC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia SMC define zonas premium, equilíbrio e desconto a partir dos recentes máximos e mínimos de swing. Opera em zonas de desconto ou premium com um filtro de tendência SMA e confirmação simples de bloco de ordens.

## Detalhes

- **Critérios de entrada**: preço na zona de desconto acima da SMA com suporte do bloco de ordens; preço na zona premium abaixo da SMA com resistência do bloco de ordens
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **Filtros**:
  - Categoria: Zone
  - Direção: Ambos
  - Indicadores: Highest, Lowest, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia ASCtrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador Williams %R para detectar reversões rápidas semelhantes à abordagem ASCtrend. Vende quando o indicador sobe de um nível de sobrevenda para um de sobrecompra e compra quando o contrário ocorre.

## Detalhes

- **Critérios de entrada**:
  - Vender quando Williams %R cruza de sobrevenda (abaixo de `x2`) para sobrecompra (acima de `x1`).
  - Comprar quando Williams %R cruza de sobrecompra (acima de `x1`) para sobrevenda (abaixo de `x2`).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal inverso fecha e inverte a posição.
- **Stops**: Não.
- **Valores padrão**:
  - `Risk` = 4
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Williams %R
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

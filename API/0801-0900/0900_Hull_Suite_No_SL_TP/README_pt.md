# Hull Suite Sem SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Hull Suite Sem SL/TP é uma estratégia de seguidor de tendência baseada em variações da Hull Moving Average. Inverte a posição quando a linha Hull muda de direção em comparação com duas velas atrás.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: O valor de Hull é maior que duas velas atrás.
  - **Vendido**: O valor de Hull é menor que duas velas atrás.
- **Critérios de saída**: Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 55
  - `Mode` = `Hma`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: Hull Moving Average
  - Complexidade: Baixo
  - Nível de risco: Baixo

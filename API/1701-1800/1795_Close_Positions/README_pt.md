# Estratégia de Fechamento de Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Fecha posições abertas com base em regras de lucro, perda ou tempo. Nenhuma nova ordem é aberta por esta estratégia.

## Detalhes

- **Critérios de entrada**: nenhum, as posições são assumidas como abertas externamente.
- **Critérios de saída**:
  - Limite de lucro ou perda em pips atingido.
  - Idade da posição supera o limite de tempo em minutos.
  - O horário atual é posterior ao horário de fechamento configurado.
- **Stops**: limites implícitos de lucro e perda.
- **Filtros**: hora do dia e tempo de manutenção.

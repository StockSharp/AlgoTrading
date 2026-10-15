# Estratégia de Posicionamento Noturno com EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Abre uma posição comprada pouco antes do fechamento do mercado selecionado e sai após a abertura do mercado. Um filtro EMA opcional confirma as entradas. A estratégia suporta sessões dos EUA, Ásia e Europa, e fecha qualquer posição aberta antes do fim de semana.

## Detalhes

- **Entrada**: Minutos antes do fechamento do mercado quando o preço está acima da EMA (se habilitado).
- **Saída**: Após a abertura do mercado pelos minutos especificados ou cinco minutos antes do fechamento de sexta-feira.
- **Mercado**: EUA, Ásia ou Europa.
- **Indicador**: EMA.
- **Direção**: Somente comprado.
- **Stops**: Nenhum.

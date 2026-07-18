# Estratégia de Virada de Mês
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este padrão sazonal compra índices de renda variável alguns dias antes do fim do mês e sai pouco depois do início do novo mês, visando capturar o efeito "virada de mês".

O sistema permanece em caixa fora desse período para reduzir a exposição.

## Detalhes

- **Dados**: Níveis diários do índice.
- **Entrada**: Comprar N dias antes do fim do mês.
- **Saída**: Vender M dias após o início do mês.
- **Instrumentos**: Futuros sobre índices de renda variável ou ETF.
- **Risco**: Sem posições fora da janela programada.


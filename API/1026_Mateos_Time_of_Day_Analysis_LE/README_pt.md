# Análise de Hora do Dia de Mateos LE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Abre uma posição comprada durante uma janela intradiária específica e a fecha mais tarde no dia.

Esta estratégia é útil para explorar os efeitos da hora do dia.

## Detalhes

- **Critérios de entrada**: O tempo atinge `StartTime` dentro do intervalo de datas `From`-`Thru`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: O tempo atinge `EndTime` (antes das 20:00).
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **Filtros**:
  - Categoria: Baseado em tempo
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

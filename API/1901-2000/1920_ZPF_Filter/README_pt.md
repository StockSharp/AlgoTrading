# Filtro de Volume ZPF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Filtro de Volume ZPF combina duas médias móveis com uma média de volume. O valor do indicador é a diferença suavizada por volume entre uma média móvel rápida e uma lenta. Quando esse valor cruza acima de zero, assume-se pressão de alta; um cruzamento abaixo de zero sinaliza pressão de baixa.

A estratégia opera em ambas as direções. As entradas ocorrem quando o indicador ZPF cruza a linha de zero. As posições são fechadas quando ocorre um cruzamento oposto.

## Detalhes

- **Critérios de entrada**: ZPF cruza acima ou abaixo de zero.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto da linha de zero.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Moving Average, Volume
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


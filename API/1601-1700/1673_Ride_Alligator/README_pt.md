# Estratégia de Seguimento do Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia Ride Alligator. O método usa três médias móveis conhecidas como o indicador Alligator. Uma posição comprada é aberta quando a linha Lips cruza acima da linha Jaws enquanto a linha Teeth está abaixo de Jaws. Uma posição vendida é aberta quando Lips cruza abaixo de Jaws e a linha Teeth está acima de Jaws. A posição aberta é protegida por um stop na linha Jaws que acompanha o movimento da linha.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - Vendido: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: `price <= Jaws`
  - Vendido: `price >= Jaws`
- **Stops**: Trailing stop no Alligator Jaws
- **Valores padrão**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Alligator
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

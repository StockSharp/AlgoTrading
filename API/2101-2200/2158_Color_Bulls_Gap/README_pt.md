# Estratégia Color Bulls Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que recria o indicador ColorBullsGap comparando gaps suavizados entre o preço máximo e as médias de abertura e fechamento.
Entra comprado quando a cor de duas barras atrás era de alta e se torna neutra ou de baixa na última barra, fechando posições vendidas.
Entra vendido quando a cor de duas barras atrás era de baixa e se torna neutra ou de alta na última barra, fechando posições compradas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `PrevColor == 0 && LastColor > 0`
  - Vendido: `PrevColor == 2 && LastColor < 2`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Length1` = 12
  - `Length2` = 5
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

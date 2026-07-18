# Estratégia Geedo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no tempo que compara os preços de abertura de duas barras passadas em uma hora específica. Se a barra mais antiga estiver acima da recente por um limite, uma operação vendida é aberta. Se a barra recente estiver acima da mais antiga, uma operação comprada é aberta. Cada posição usa stop loss e take profit fixos e é fechada após um tempo máximo de manutenção.

## Detalhes

- **Critérios de entrada**: Em `TradeTime` comparar preços de abertura de `T1` e `T2` barras atrás. Se `Open[T1] - Open[T2]` exceder `DeltaShort`, vender; se `Open[T2] - Open[T1]` exceder `DeltaLong`, comprar.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss, take profit ou `MaxOpenTime` horas após a entrada.
- **Stops**: Stop loss e take profit fixos em pontos.
- **Valores padrão**:
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **Filtros**:
  - Categoria: Baseada em tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Fixo
  - Complexidade: Iniciante
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

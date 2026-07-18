# Estratégia Knux de Múltiplos Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina indicadores de força de tendência e osciladores de momentum para operar rompimentos. Ela aguarda um cruzamento de alta ou baixa de duas médias móveis enquanto o Average Directional Index (ADX) sinaliza uma tendência forte. O Relative Vigor Index (RVI), o Commodity Channel Index (CCI) e Williams %R atuam como filtros para garantir que o momentum confirme o movimento e que o mercado não esteja sobreextendido.

O sistema pode abrir posições compradas e vendidas. Ele mantém a posição até que apareça um sinal oposto e não utiliza um stop-loss dedicado. Todos os parâmetros, como períodos e limites dos indicadores, são configuráveis.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápida cruza acima da SMA lenta, `ADX > AdxLevel`, `RVI` em ascensão, `CCI < -CciLevel`, e `WPR <= -100 + WprBuyRange`.
  - **Vendido**: SMA rápida cruza abaixo da SMA lenta, `ADX > AdxLevel`, `RVI` em queda, `CCI > CciLevel`, e `WPR >= -WprSellRange`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto (cruzamento na direção contrária).
- **Stops**: Sem stop-loss explícito.
- **Valores padrão**:
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Nenhum
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

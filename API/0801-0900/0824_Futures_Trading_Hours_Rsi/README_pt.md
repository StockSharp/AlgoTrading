# Estratégia de Futuros em Horário de Negociação com RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera apenas durante o horário da sessão de futuros americanos (08:30–15:00 CT). Usa o Índice de Força Relativa (RSI) para entrar comprado quando o oscilador cruza acima do nível de sobrevenda e entrar vendido quando cruza abaixo do nível de sobrecompra. Às 15:00 CT ou após, todas as posições abertas são fechadas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI cruza acima do nível de sobrevenda durante a sessão
  - **Vendido**: RSI cruza abaixo do nível de sobrecompra durante a sessão
- **Comprado/Vendido**: Ambos os lados
- **Critérios de saída**:
  - Todas as posições fechadas ao final da sessão (15:00 CT)
- **Stops**: Nenhum
- **Valores padrão**:
  - `RsiLength` = 14
  - `OverSoldLevel` = 30
  - `OverBoughtLevel` = 70
  - `SessionStart` = 08:30
  - `SessionEnd` = 15:00
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

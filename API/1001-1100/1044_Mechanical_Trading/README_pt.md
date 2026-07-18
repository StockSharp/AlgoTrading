# Estratégia de Trading Mecânico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia mecânica baseada em tempo que executa uma operação em uma hora especificada a cada dia. A direção da posição pode ser configurada para comprado ou vendido. A operação é protegida automaticamente com níveis de take profit e stop loss baseados em porcentagem.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: em `TradeHour` quando `Short Mode` está desativado.
  - **Vendido**: em `TradeHour` quando `Short Mode` está ativado.
- **Comprado/Vendido**: Ambos, dependendo de `Short Mode`.
- **Critérios de saída**:
  - `Profit Target (%)` acima/abaixo da entrada.
  - `Stop Loss (%)` abaixo/acima da entrada.
- **Stops**: Stop loss e take profit.
- **Valores padrão**:
  - `Profit Target (%)` = 0.4.
  - `Stop Loss (%)` = 0.2.
  - `Trade Hour` = 16.
- **Filtros**:
  - Categoria: Tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

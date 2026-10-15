# Estratégia Improvisando
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Improvisando combina um filtro de tendência EMA básico com oscilações do RSI. O objetivo é seguir a direção prevalecente indicada pela EMA enquanto entra apenas quando o RSI cruza a linha neutra de 50. O design original também experimentou com momentum no estilo MACD, mas esta versão simplificada foca na clareza e facilidade de ajuste.

O usuário pode habilitar operações compradas e/ou vendidas separadamente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close > EMA` e `RSI > 50`
  - **Vendido**: `Close < EMA` e `RSI < 50`
- **Comprado/Vendido**: Configurável
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `EmaLength` = 10
  - `RsiLength` = 14
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: EMA, RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

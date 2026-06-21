# Estratégia de Rompimento Bitcoin 1H-15M
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia rastreia a máxima e a mínima do candle de 1 hora anterior e entra em operações quando um candle de 15 minutos fecha fora desse intervalo. O risco é gerenciado com um buffer de stop-loss fixo e um take-profit derivado de uma relação risco-retorno configurável.

## Detalhes

- **Critérios de entrada**:
  - Fechamento de 15 minutos acima da máxima da hora anterior → entrada comprada.
  - Fechamento de 15 minutos abaixo da mínima da hora anterior → entrada vendida.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop loss a distância de buffer fixo.
  - Take profit a buffer × relação risco-retorno.
- **Stops**: Stop loss e take profit via módulo de proteção.
- **Valores padrão**:
  - Período inferior = 15 minutos.
  - Período superior = 1 hora.
  - Buffer de stop loss = 50.
  - Relação risco-retorno = 2.0.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: SL & TP
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia S4 IBS de Reversão à Média com Saída em 3 Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando a Força Interna da Barra (IBS) da vela anterior está abaixo de um limiar, esperando uma reversão à média. Sai quando o preço fecha acima da entrada ou após três velas se a operação continuar perdedora.

## Detalhes

- **Critérios de entrada**: IBS anterior <= limiar
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: fechamento acima do preço de entrada ou após 3 velas se ainda abaixo da entrada; saída forçada após o horário final
- **Stops**: Não
- **Valores padrão**:
  - `IbsThreshold` = 0.25
  - `StartTime` = 2024-01-01 05:00:00 UTC
  - `EndTime` = 2024-12-31 00:00:00 UTC
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

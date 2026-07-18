# Estratégia da Primeira Vela de 30m do Índice US
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Captura o rompimento do intervalo dos primeiros 30 minutos da sessão americana com uma operação por dia.

## Detalhes

- **Critérios de entrada**: Após o intervalo dos primeiros 30m ser fixado, o preço rompe acima da máxima ou abaixo da mínima
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop no nível oposto do intervalo, alvo no tamanho do intervalo * risco/retorno
- **Stops**: Sim
- **Valores padrão**:
  - `RiskReward` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

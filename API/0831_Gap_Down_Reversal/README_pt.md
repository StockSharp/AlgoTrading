# Estratégia de Reversão por Gap de Baixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Reversão por Gap de Baixa busca reversões de alta após uma abertura com gap de queda.
Quando uma nova sessão abre abaixo da mínima anterior mas fecha acima de sua abertura, frequentemente aprisiona vendedores e sinaliza uma recuperação.

A estratégia entra comprado quando esse padrão aparece e sai quando o preço fecha acima da máxima anterior.

## Detalhes

- **Critérios de entrada**: padrão de reversão por gap de baixa
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: fechamento acima da máxima anterior
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 day
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

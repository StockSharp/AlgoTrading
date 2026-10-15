# Estratégia de Negociação Dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de negociação de pares que abre posições opostas em dois instrumentos correlacionados e as fecha quando o lucro combinado atinge um alvo.

## Detalhes

- **Critérios de entrada**: abrir simultaneamente a primeira e a segunda posição em direções opostas
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**: lucro combinado >= ProfitTarget
- **Stops**: Não
- **Valores padrão**:
  - `Volume1` = 1
  - `Volume2` = 1.3
  - `ProfitTarget` = 20
  - `SecondSecurity` = obrigatório
- **Filtros**:
  - Categoria: Negociação de pares
  - Direção: Protegido
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

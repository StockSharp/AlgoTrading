# Estratégia de Cruzamento Fisher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador Fisher Transform para entrar em posições compradas quando o indicador cruza acima do seu valor anterior enquanto está abaixo de 1. As posições são fechadas quando o indicador cruza abaixo do seu valor anterior enquanto está acima de 1.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Fisher crosses above previous Fisher` && `Fisher < 1`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - `Fisher crosses below previous Fisher` && `Fisher > 1`
- **Stops**: Não
- **Valores padrão**:
  - `Fisher Length` = 9
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Fisher Transform
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

# Estratégia HOD/LOD/PMH/PML/PDH/PDL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de níveis do pré-mercado e do dia anterior.
Entradas compradas ocorrem quando o preço cruza acima do máximo do pré-mercado ou do dia anterior.
Entradas vendidas ocorrem quando o preço cruza abaixo do mínimo do pré-mercado ou do dia anterior.
As posições são fechadas quando o preço atinge a máxima ou mínima do dia atual.

## Detalhes

- **Critérios de entrada**: preço cruzando os níveis do pré-mercado ou do dia anterior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: atingir a máxima ou mínima do dia atual
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Níveis
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

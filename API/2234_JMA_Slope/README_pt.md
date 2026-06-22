# Estratégia de Inclinação JMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora a inclinação da Média Móvel Jurik (JMA). Uma posição é aberta quando a inclinação cruza zero ou quando sua direção muda dependendo do modo selecionado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A inclinação cruza abaixo de zero ou vira para cima (dependente do modo).
  - **Vendido**: A inclinação cruza acima de zero ou vira para baixo.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - O sinal oposto inverte a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `JMA Length` = 14
  - `JMA Phase` = 0
  - `Mode` = Breakdown
  - `Candle Type` = Período 4h
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: JMA
  - Stops: Não
  - Complexidade: Básico
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

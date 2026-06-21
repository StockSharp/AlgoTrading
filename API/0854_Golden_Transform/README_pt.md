# Estratégia de Transformação Dourada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o indicador Rate of Change com um TRIX triplo baseado em Hull, um filtro Hull MA e um Fisher Transform suavizado. Operações compradas são abertas quando o ROC cruza acima do TRIX enquanto o TRIX está abaixo de zero e o preço de abertura está acima do Hull MA. Operações vendidas ocorrem no sinal oposto. As posições são fechadas em cruzamentos opostos ou quando o Fisher suavizado ultrapassa os limites e se reverte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `ROC crosses above TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **Vendido**: `ROC crosses below TRIX` && `TRIX > 0` && `Open < Hull MA`
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**:
  - Comprado: `ROC crosses below TRIX` OU (`Fisher HMA > 1.5` && `Fisher HMA crosses below previous Fisher`)
  - Vendido: `ROC crosses above TRIX` OU (`Fisher HMA < -1.5` && `Fisher HMA crosses above previous Fisher`)
- **Stops**: Não
- **Valores padrão**:
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ROC, Hull MA, Fisher Transform
  - Stops: Não
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

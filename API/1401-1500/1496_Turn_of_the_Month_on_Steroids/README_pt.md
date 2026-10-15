# Estratégia de Virada do Mês on Steroids
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia sazonal que compra perto do final de cada mês após dois fechamentos consecutivos de queda e sai quando um RSI curto sinaliza condições de sobrecompra.

## Detalhes

- **Critérios de entrada**: dia do mês acima do limiar e queda de dois dias
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: RSI acima do limiar
- **Stops**: Nenhum
- **Valores padrão**:
  - `DayOfMonth` = 25
  - `RsiLength` = 2
  - `RsiThreshold` = 65
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
